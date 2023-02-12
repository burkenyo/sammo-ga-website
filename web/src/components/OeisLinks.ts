import { getLinkProps } from "@/vue-utils";
import { h, Text, type FunctionalComponent, type VNode } from "vue";

// Wraps OEIS IDs encountered in text nodes with hyperlinks
export default <FunctionalComponent>((_, { slots: { default: slot } }) => {
  if (!slot) {
    return null;
  }

  const nodes: VNode[] = [];

  for (const node of slot()) {
    if (node.type != Text) {
      nodes.push(node);
      continue;
    }

    const text = node.children as string;

    let index = 0;
    for (const match of text.matchAll(/A0*[1-9]\d{0,8}/g)) {
      if (match.index! > 0) {
        nodes.push(h(Text, text.substring(index, match.index!)));
      }

      const oeisId = match[0];

      nodes.push(h("a", getLinkProps("https://oeis.org/" + oeisId), oeisId));
      index += match.index! + oeisId.length;
    }

    if (index < text.length) {
      nodes.push(h(Text, text.substring(index)));
    }
  }

  return nodes;
});
