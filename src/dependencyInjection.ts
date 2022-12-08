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
  private readonly _implementingClass: ServiceClass<TInt> | null;
  private readonly _style: ServiceStyle;

  readonly key: ServiceKey<TInt>;
  readonly lifetime: ServiceLifetime;
  readonly factory: ServiceFactoryWithString<TInt>;

  private constructor(key: ServiceKey<TInt>, lifetime: ServiceLifetime, style: ServiceStyle,
    factory: ServiceFactoryWithString<TInt>, implementingClass: ServiceClass<TInt> | null
  ) {
    assert(style == ServiceStyle.Injected != !implementingClass,
      "implementingClass must be provided if and only if style is ServiceStyle.Injected!");

    this.key = key;
    this.lifetime = lifetime;
    this._style = style;
    this.factory = factory;
    this._implementingClass = implementingClass;
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
    const name = this._implementingClass?.name ?? this.key.description ?? "(anonymous)";
    const lifetimeString = ServiceLifetime[this.lifetime].toLowerCase();
    const styleString = ServiceStyle[this._style].toLowerCase();

    return `<${lifetimeString} ${styleString} of ${name}>`;
  }
}

export class ContainerBuilder {
  private readonly _services = new Map<ServiceKey<object>, ServiceDescriptor<object>>();

  registerSingletonInstance<TInt extends Object>(key: ServiceKey<TInt>, instance: TInt) {
    const descriptor = ServiceDescriptor.forSingletonInstance(key, instance);

    this._services.set(key, descriptor);
  }

  registerFactory<TInt extends object>(
    lifetime: ServiceLifetime, key: ServiceKey<TInt>, factory: ServiceFactory<TInt>
  ) {
    const descriptor = ServiceDescriptor.forFactory(lifetime, key, factory);

    this._services.set(key, descriptor);
  }

  registerInjected<TInt extends object>(
    lifetime: ServiceLifetime, key: ServiceKey<TInt>, impl: ServiceClass<TInt>
  ) {
    const descriptor = ServiceDescriptor.forInjected(lifetime, key, impl);

    this._services.set(key, descriptor);
  }

  build(): Container {
    const container = new DefaultContainer(new Map(this._services));

    return container;
  }
}

export interface Container {
  retrieve<TInt extends object>(key: ServiceKey<TInt>): TInt;
}

class DefaultContainer implements Container {
  private readonly _validatedKeys = new Set<ServiceKey<object>>();
  private readonly _singletonCache = new Map<ServiceKey<object>, object>();
  private readonly _services: ReadonlyMap<ServiceKey<object>, ServiceDescriptor<object>>;
  private readonly _nameMap: ReadonlyMap<string, ServiceKey<object>>;

  constructor(services: ReadonlyMap<ServiceKey<object>, ServiceDescriptor<object>>) {
    this._services = services;

    const nameMap = new Map<string, ServiceKey<object>>();
    for (const key of services.keys()) {
      nameMap.set(key.description!, key);
    }

    this._nameMap = nameMap;
  }

  retrieve<TInt extends object>(key: ServiceKey<TInt>): TInt {
    return this.retrieveInternal(key, null, null);
  }

  private retrieveInternal<TInt extends object>(
    key: ServiceKey<TInt> | string, stack: Set<ServiceKey<object>> | null, requiredBy: ServiceDescriptor<object> | null
  ): TInt {
    if (typeof key === "string") {
      const lookedUp = this._nameMap.get(key);

      if (!lookedUp) {
        throw new TypeError(`No service key found for “${key}”`);
      }

      key = lookedUp;
    }

    const descriptor = this._services.get(key);

    if (!descriptor) {
      const message = key.description
        ? `No service was registered for the given key “${key.description}”!`
        : "No service was registered for the given key!";

      throw new TypeError(message);
    }

    const validated = this._validatedKeys.has(key);

    // Singleton services are cached because they shouldn’t get rebuilt
    const cached = this._singletonCache.get(key) as TInt | undefined;
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
        const descriptors = [...stack].map(k => this._services.get(k));
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

    const instance = descriptor.factory(key => this.retrieveInternal(key, stack, descriptor));

    if (descriptor.lifetime == ServiceLifetime.Singleton) {
      this._singletonCache.set(key, instance);
    }

    this._validatedKeys.add(key);

    stack?.delete(key);

    return instance as TInt;
  }
}
