/// <reference types="vite/client" />
/// <reference types="vite-plugin-pages/client" />

interface ImportMetaEnv {
  readonly VITE__API_BASE_URL: Optional<string>;
  readonly VITE__USE_MOCK_API: Optional<string>;
  readonly VITE__GIT_BRANCH: Optional<string>;
  readonly VITE__GIT_COMMIT: Optional<string>;
}
