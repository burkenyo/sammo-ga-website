import type { Ref } from "vue";
import { CancellablePromise, delay } from "@/utils";

const STORAGE_KEY = "givenName";

const FAMILY_NAME = "Gabay";

const GIVEN_NAMES: readonly string[] = [
  "Sammo",
  "Samuel Justin"
];

let oldGivenName: string;
let nameCounter: number;
let newGivenName: string;

function setup() {
  // get the name that was last displayed
  const storedName = localStorage.getItem(STORAGE_KEY) ?? "";
  const index = GIVEN_NAMES.indexOf(storedName);

  if (index == -1) {
    oldGivenName = GIVEN_NAMES[0];
    nameCounter = 1;
  } else {
    oldGivenName = GIVEN_NAMES[index];
    nameCounter = (index + 1) % GIVEN_NAMES.length;
  }

  newGivenName = GIVEN_NAMES[nameCounter];
}

async function removeOldGivenName(nameRef: Ref<string>): Promise<void> {
  const numUpdates = oldGivenName.length;
  for (let i = 1; i < numUpdates; i++) {
    await delay(120);

    nameRef.value = oldGivenName.substring(0, oldGivenName.length - i) + " " + FAMILY_NAME;
  }
}

async function prependNewGivenName(nameRef: Ref<string>): Promise<void> {
  const numUpdates = newGivenName.length;
  for (let i = 2; i <= numUpdates; i++) {
    await delay(120);

    nameRef.value = newGivenName.substring(0, i) + " " + FAMILY_NAME;
  }
}

export function useNameUpdater(nameRef: Ref<string>): () => void {
  setup();

  nameRef.value = oldGivenName + " " + FAMILY_NAME;

  const canceler = new CancellablePromise();

  (async () => {
    while (!canceler.isCanceled) {
      await Promise.race([delay(20000), canceler]);

      if (canceler.isCanceled) {
        return;
      }

      // stash the name that is to be displayed next
      localStorage.setItem(STORAGE_KEY, newGivenName);

      await removeOldGivenName(nameRef);

      await delay(1000);

      await prependNewGivenName(nameRef);

      oldGivenName = newGivenName;
      newGivenName = GIVEN_NAMES[++nameCounter % GIVEN_NAMES.length];
    }
  })();

  return () => canceler.cancel();
}
