// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

export { }

declare module "vue-router" {
  interface RouteMeta {
    readonly title: string;
    readonly description: string;
    readonly simpleLayoutHeading?: Optional<string>;
    readonly menuOrder?: Optional<number>;
  }
}
