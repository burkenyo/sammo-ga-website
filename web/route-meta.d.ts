export { }

declare module "vue-router" {
  interface RouteMeta {
    readonly title: string;
    readonly description: string;
    readonly simpleLayoutHeading?: Optional<string>;
    readonly menuOrder?: Optional<number>;
  }
}
