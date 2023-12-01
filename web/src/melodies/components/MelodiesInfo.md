<!-- Copyright © 2023 Samuel Justin Gabay
     Licensed under the GNU Affero Public License, Version 3 -->

<!-- lang="ts" removed for to fix builds breaking with unplugin-vue-markdown -->
<script setup>
import { MAX_PERMUTATION } from "@melodies/state";
import ConstantIcon from "@melodies/components/ConstantIcon.vue"

const maxPermutation = MAX_PERMUTATION.toLocaleString();
</script>

### Info

This page generates melodies by mapping the digits of mathematical constants and other decimal expansions expressed in dozenal (base-12) notation to notes of the Western 12-tone chromatic scale. Dozenal notation provides 12 possible values for each digit as opposed to the 10 found in decimal notation, where each position represents a power of 12: the one’s (12^0^ = 1) place, the dozen’s (12^1^ = 12) place, the gross’ (12^2^ = 144) place, etc. The analogy extends to the other side of the fractional point with the negative powers of 12: the “dozen-th’s” (12^-1^ = ^1^/~12~) place, the “gross-th’s” (12^-2^ = ^1^/~144~) place, etc. Thus, any number can be represented in dozenal notation, and since there are twelve digits, we can uniquely each digit to a note to turn the number into a melody.

#### But how should we do the mapping?
The simplest way would be to say each __0__ in the expansion becomes a __C__ in the melody, each __1__ becomes a __C♯__, each __2__ becomes a __D__ and so on all the way up to each __11__ becomes a __B__. But this is only one possible mapping, and we could just as easily map each digit to a note in a seemingly fashion (for example, __0__ → __F__, __1__ → __G♯__, __2__ → __E__, ...) so long as the mapping is unique (each digits maps to exactly one note). Each mapping is therefore a permutation of the chromatic scale, the order of which maps to the dozenal digits. There are an incredible __{{ maxPermutation }}__ (11-factorial) possible permutations!

#### Since we’re dealing with base-12, why aren’t there 12-factorial possible permutations?
Technically there are, but because the chromatic scale is cyclical, there are 12 permutations which are simply transpositions, musically speaking, of each other. For example, take the trivial mapping above (__0__ → __C__, etc.) and shift each note a half-step up such that __0__ → __C♯__, __1__ → __D__, all the way up to __10__ → __B__, and __11__ → __C__. We’ve just transposed the whole mapping up a half-step. So, we’ll use the convention that each of the {{ maxPermutation }} permutations by default starts with __0__ → __C__, and then we can apply a transposition of up to 11 half-steps.

#### Where do the expansions come from?
The [Online Encyclopedia of Integer Sequences](https://oeis.org) contains hundreds of thousands of entries. A good chunk of these contain the decimal expansions of irrational numbers such as <ConstantIcon tag="pi" /> and <ConstantIcon tag="e" /> and other interesting numbers to thousands of places. By converting these expansions to dozenal, we have myriad sources of digits to map through the note mappings and generate melodies. In the fields below, you’ll see the A-number of the sequences from which the expansions are sourced.

#### How to use this page
Try experimenting with different constants and mappings. The resulting melody is notated at the bottom. You can enter an A-number of your choosing to have it looked up in the OEIS or have a sequence chosen randomly. Note that not all OEIS sequences represent numeric expansions, so not all A-numbers will work. On permutation side, you can enter any number from 1 to {{ maxPermutation }} and any transposition from 0 to 11. You can pick a random permutation or reverse it, reflect it (get the musical inversion), or invert it (mathematically speaking). Some operations don’t appear to do anything because performing the operations yields the same permutation. For example, inverting permutation 1, transposition 1 or 6, doesn’t appear to do anything because those permutations are there own inverses.
