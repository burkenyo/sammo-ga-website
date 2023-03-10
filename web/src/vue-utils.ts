// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

export function getLinkProps(href: string): { href: string; target?: string } {
  if (href.startsWith("http")) {
    // external link will open up in a new tab
    return { href, target: "_blank" };
  }

  return { href };
}
