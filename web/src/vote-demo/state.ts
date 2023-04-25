// Copyright © 2023 Samuel Justin Gabay
// Licensed under the GNU Affero Public License, Version 3

import { defineStore } from "pinia";
import { ref } from "vue";

export const useState = defineStore("state", () => ({
  showSuccess: ref(false),
}));
