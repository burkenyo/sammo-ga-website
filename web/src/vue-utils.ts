export function getLinkProps(href: string): { href: string; target?: string } {
  if (href.startsWith("http")) {
    // external link will open up in a new tab
    return { href, target: "_blank" };
  }

  return { href };
}
