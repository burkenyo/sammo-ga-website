// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { useServerHead } from "@vueuse/head";
import { isTrue, TRUE_STRING } from "./utils";

interface BuildInfo {
  readonly gitBranch: string;
  readonly gitCommit: string;
  readonly isDirty: boolean;
  readonly isBuilt: boolean;
}

export function useBuildInfo(): BuildInfo {
  const gitBranch = import.meta.env.VITE__GIT_BRANCH!;
  const gitCommit = import.meta.env.VITE__GIT_COMMIT!;
  const isDirty = isTrue(import.meta.env.VITE__GIT_IS_DIRTY);
  const isBuilt = import.meta.env.VITE__COMMAND == "build";

  return { gitBranch, gitCommit, isDirty, isBuilt };
}

export function useGitInfoMeta(): void {
  if (import.meta.env.SSR) {
    // bake git info into the meta tags during build
    const buildInfo = useBuildInfo();

    const gitInfo = [
      { name: "git-branch", content: buildInfo.gitBranch },
      { name: "git-commit", content: buildInfo.gitCommit },
    ];

    if (buildInfo.isDirty) {
      gitInfo.push({ name: "git-is-dirty", content: TRUE_STRING });
    }

    useServerHead({
      meta: gitInfo,
    });
  }
}
