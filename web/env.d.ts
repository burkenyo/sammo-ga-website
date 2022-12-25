/// <reference types="vite/client" />
/// <reference types="vite-plugin-pages/client" />

interface ImportMetaEnv {
  readonly VITE__API_BASE_URL: string | undefined;
  readonly VITE__USE_MOCK_API: string | undefined;
}
