/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE__API_BASE_URL: Optional<string>;
  readonly VITE__USE_MOCK_API: Optional<string>;
  readonly VITE__GIT_BRANCH: Optional<string>;
  readonly VITE__GIT_COMMIT: Optional<string>;
  readonly VITE__GIT_IS_DIRTY: Optional<string>;
  readonly VITE__ASSETS_BASE_URL: Optional<string>;
  readonly VITE__COMMAND: Optional<string>;
}
