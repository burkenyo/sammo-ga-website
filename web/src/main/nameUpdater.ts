// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { ref, type Ref } from "vue";
import { CancellablePromise, delay } from "@shared/utils";

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
  const storedName = localStorage.getItem(STORAGE_KEY);
  const index = storedName ? GIVEN_NAMES.indexOf(storedName) : -1;

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

export function useStaticName(): string {
  return GIVEN_NAMES[0] + " " + FAMILY_NAME;
}

export function useNameUpdater(): Readonly<{ nameRef: Ref<string>, canceler: () => void }> {
  setup();

  const nameRef = ref(oldGivenName + " " + FAMILY_NAME);

  const canceler = new CancellablePromise();

  (async () => {
    while (!canceler.isCanceled) {
      await Promise.race([delay(30000), canceler]);

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

  return { nameRef, canceler: () => canceler.cancel() };
}
