import { assert } from "./utils";

export const dependencies: unique symbol = Symbol();

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export interface ServiceKey<TInt extends object> extends Symbol { }

export function serviceKey<TInt extends object>(name: string): ServiceKey<TInt> {
  if (!name) {
    throw new TypeError("name");
  }

  return Symbol(name) as ServiceKey<TInt>;
}

export interface ServiceClass<TInt extends object> {
  new (...args: any[]): TInt;
  readonly [dependencies]?: Readonly<string[]>;
}

export interface ServiceProvider {
  <TInt extends object>(key: ServiceKey<TInt>): TInt;
}

export interface ServiceFactory<TInt extends object> {
  (provider: ServiceProvider): TInt;
}

interface ServiceProviderWithString {
  (key: ServiceKey<object> | string): object;
}

interface ServiceFactoryWithString<TInt extends object> {
  (provider: ServiceProviderWithString): TInt;
}

// These numeric values of these members must be ordered according to their logical lifetimes
// i.e. A member with a shorter lifetime must have a value less than a member with a longer lifetime
export enum ServiceLifetime {
  Transient,
  Singleton,
}

enum ServiceStyle {
  Instance,
  Factory,
  Injected,
}

class ServiceDescriptor<TInt extends object> {
  readonly #implementingClass: Optional<ServiceClass<TInt>>;
  readonly #style: ServiceStyle;

  readonly key: ServiceKey<TInt>;
  readonly lifetime: ServiceLifetime;
  readonly factory: ServiceFactoryWithString<TInt>;

  private constructor(key: ServiceKey<TInt>, lifetime: ServiceLifetime, style: ServiceStyle,
    factory: ServiceFactoryWithString<TInt>, implementingClass: Optional<ServiceClass<TInt>>
  ) {
    assert(style == ServiceStyle.Injected != !implementingClass,
      "implementingClass must be provided if and only if style is ServiceStyle.Injected!");

    this.key = key;
    this.lifetime = lifetime;
    this.#style = style;
    this.factory = factory;
    this.#implementingClass = implementingClass;
  }

  static forSingletonInstance<TInt extends object>(
    key: ServiceKey<TInt>, instance: TInt
  ): ServiceDescriptor<TInt> {
    const factory = () => instance;

    return new ServiceDescriptor(key, ServiceLifetime.Singleton, ServiceStyle.Instance, factory, null);
  }

  static forFactory<TInt extends object>(
    lifetime: ServiceLifetime, key: ServiceKey<TInt>, factory: ServiceFactory<TInt>
  ): ServiceDescriptor<TInt> {
    // Here I assert that the given ServiceProviderWithString sent to factory
    // will behave like a generic ServiceProvider, and, importantly,
    // return the correct types when called with typed keys!
    return new ServiceDescriptor(key, lifetime, ServiceStyle.Factory, factory as ServiceFactoryWithString<TInt>, null);
  }

  static forInjected<TInt extends object>(
    lifetime: ServiceLifetime, key: ServiceKey<TInt>, impl: ServiceClass<TInt>
  ): ServiceDescriptor<TInt> {
    const numDependencies = impl[dependencies]?.length ?? 0;

    if (numDependencies != impl.length) {
      const message = `Dependency count mismatch! ${impl.name} specifies ${numDependencies} dependencies`
        + ` but has a constructor that takes ${impl.length} arguments.`;

      throw new TypeError(message);
    }

    const factory = (provider: ServiceProviderWithString) => {
      if (impl[dependencies]) {
        const deps = impl[dependencies].map(n => provider(n));

        return new impl(...deps) as TInt;
      } else {
        return new impl() as TInt;
      }
    };

    return new ServiceDescriptor(key, lifetime, ServiceStyle.Injected, factory, impl);
  }

  toString(): string {
    const name = this.#implementingClass?.name ?? this.key.description ?? "(anonymous)";
    const lifetimeString = ServiceLifetime[this.lifetime].toLowerCase();
    const styleString = ServiceStyle[this.#style].toLowerCase();

    return `<${lifetimeString} ${styleString} of ${name}>`;
  }
}

export class ContainerBuilder {
  readonly #services = new Map<ServiceKey<object>, ServiceDescriptor<object>>();

  registerSingletonInstance<TInt extends object>(
    key: ServiceKey<TInt>, instance: TInt
  ) {
    const descriptor = ServiceDescriptor.forSingletonInstance(key, instance);

    this.#services.set(key, descriptor);

    return this;
  }

  registerFactory<TInt extends object>(
    lifetime: ServiceLifetime, key: ServiceKey<TInt>, factory: ServiceFactory<TInt>
  ) {
    const descriptor = ServiceDescriptor.forFactory(lifetime, key, factory);

    this.#services.set(key, descriptor);

    return this;
  }

  registerInjected<TInt extends object>(
    lifetime: ServiceLifetime, key: ServiceKey<TInt>, impl: ServiceClass<TInt>
  ) {
    const descriptor = ServiceDescriptor.forInjected(lifetime, key, impl);

    this.#services.set(key, descriptor);

    return this;
  }

  build(): Container {
    const container = new DefaultContainer(new Map(this.#services));

    return container;
  }
}

export interface Container {
  retrieve<TInt extends object>(key: ServiceKey<TInt>): TInt;
}

class DefaultContainer implements Container {
  readonly #validatedKeys = new Set<ServiceKey<object>>();
  readonly #singletonCache = new Map<ServiceKey<object>, object>();
  readonly #services: ReadonlyMap<ServiceKey<object>, ServiceDescriptor<object>>;
  readonly #nameMap: ReadonlyMap<string, ServiceKey<object>>;

  constructor(services: ReadonlyMap<ServiceKey<object>, ServiceDescriptor<object>>) {
    this.#services = services;

    const nameMap = new Map<string, ServiceKey<object>>();
    for (const key of services.keys()) {
      nameMap.set(key.description!, key);
    }

    this.#nameMap = nameMap;
  }

  retrieve<TInt extends object>(key: ServiceKey<TInt>): TInt {
    return this.#retrieve(key, null, null);
  }

  #retrieve<TInt extends object>(
    key: ServiceKey<TInt> | string, stack: Optional<Set<ServiceKey<object>>>, requiredBy: Optional<ServiceDescriptor<object>>
  ): TInt {
    if (typeof key === "string") {
      const lookedUp = this.#nameMap.get(key);

      if (!lookedUp) {
        const message = requiredBy
          ? `No service key found for “${key}”! Required by ${requiredBy}.`
          : `No service key found for “${key}”!`;

        throw new TypeError(message);
      }

      key = lookedUp;
    }

    const descriptor = this.#services.get(key);

    if (!descriptor) {
      const message = key.description
        ? `No service was registered for the given key “${key.description}”!`
        : "No service was registered for the given key!";

      throw new TypeError(message);
    }

    const validated = this.#validatedKeys.has(key);

    // Singleton services are cached because they shouldn’t get rebuilt
    const cached = this.#singletonCache.get(key) as TInt | undefined;
    if (cached) {
      assert(validated,
        "Expected descriptor to be validated when returning a cached instance! Service: ", descriptor);

      assert(descriptor.lifetime == ServiceLifetime.Singleton,
        "Unexpected cached instance for non-singleton service! Service: ", descriptor);

      return cached;
    }

    if (!validated) {
      stack ??= new Set();

      if (stack.has(key)) {
        const descriptors = [...stack].map(k => this.#services.get(k));
        descriptors.push(descriptor);

        const message = "Cyclic dependency detected! Cycle was: " + descriptors.join(" → ");

        throw new TypeError(message);
      }

      stack.add(key);

      if (requiredBy && descriptor.lifetime < requiredBy.lifetime) {
        const message = `Service ${requiredBy} cannot require service ${descriptor} because it has a longer lifetime!`;

        throw new TypeError(message);
      }
    }

    const instance = descriptor.factory(key => this.#retrieve(key, stack, descriptor));

    if (descriptor.lifetime == ServiceLifetime.Singleton) {
      this.#singletonCache.set(key, instance);
    }

    this.#validatedKeys.add(key);

    stack?.delete(key);

    return instance as TInt;
  }
}
