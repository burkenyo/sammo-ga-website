/// <reference types="vite/client" />
// ensure “import routes from "~pages"” passes type checks
/// <reference types="vite-plugin-pages/client" />

interface ImportMetaEnv {
  readonly VITE__API_BASE_URL: Optional<string>;
  readonly VITE__USE_MOCK_API: Optional<string>;
  readonly VITE__GIT_BRANCH: Optional<string>;
  readonly VITE__GIT_COMMIT: Optional<string>;
}

import "vue-router";

declare module "vue-router" {
  interface RouteMeta {
    readonly title: string;
    readonly description: string;
    readonly simpleLayoutHeading?: Optional<string>;
    readonly menuOrder?: Optional<number>;
  }
}
