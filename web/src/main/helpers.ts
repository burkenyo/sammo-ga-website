// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

export function localUrlForSubApp(url: string) {
  return import.meta.env.MODE == "production"
    ? url // Web host handles bare URL fine.
    : url + "/"; // Local dev needs terminal slash.
}
