// Copyright © 2025 Samuel Justin Speth Gabay
// Licensed under the GNU Affero Public License, Version 3

export { }

declare module "vue-router" {
  interface RouteMeta {
    readonly title: string;
    readonly description: string;
  }
}
