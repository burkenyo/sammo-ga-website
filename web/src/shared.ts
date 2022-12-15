import { defineStore } from "pinia";
import { computed, shallowRef } from "vue";
import { OeisId } from "./oeis";
import { Permutation } from "./permutation";

interface InterestingConstant {
  readonly tag: string;
  readonly id: OeisId;
  readonly description: string;
}

export const interestingConstantsInfo: readonly InterestingConstant[] = [
  {
    tag: "pi",
    id: new OeisId(796),
    description: "Pi, the ratio of a circle’s circumference to its diameter",
  },
  {
    tag: "e",
    id: new OeisId(1113),
    description: "The base of the natural logarithm",
  },
  {
    tag: "gamma",
    id: new OeisId(1620),
    description: "Gamma, the Euler-Mascheroni constant",
  },
  {
    tag: "phi",
    id: new OeisId(1622),
    description: "Phi, the golden ratio",
  },
  {
    tag: "root_two",
    id: new OeisId(2193),
    description: "The square-root of two",
  },
  {
    tag: "twelfth_root_two",
    id: new OeisId(10774),
    description: "The ratio of the pitches of any two neighboring notes on the chromatic scale",
  },
  {
    tag: "tau",
    id: new OeisId(19692),
    description: "Tau, two times pi, the ratio of a circle’s circumference to its radius",
  },
];

const NOTES = ["C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B"] as const;
export const BASE = NOTES.length;

// state uses immutable domain types, hence shallow refs;
// this also means watchers need to be aware of “false positives”,
// e.g. instance where shared the permutation or oeisId objects were replaced,
// but their values are still equal
export const useState = defineStore("state", () => {
  const permutation = shallowRef(Permutation.create(BASE, 1, 0));
  const oeisId = shallowRef(interestingConstantsInfo[0].id);
  const noteSequence = computed(() => permutation.value.sequence.map(e => NOTES[e]));

  const randomizePermutation = () => permutation.value = Permutation.createRandom(BASE);
  const reversePermutation = () => permutation.value = permutation.value.reverse();
  const reflectPermutation = () => permutation.value = permutation.value.reflect();
  const invertPermutation = () => permutation.value = permutation.value.invert();

  return {
    permutation,
    oeisId,
    noteSequence,
    randomizePermutation,
    reversePermutation,
    reflectPermutation,
    invertPermutation,
  };
});
