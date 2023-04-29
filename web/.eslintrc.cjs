/* eslint-env node */
require("@rushstack/eslint-patch/modern-module-resolution");

module.exports = {
  root: true,
  extends: [
    "plugin:vue/vue3-essential",
    "eslint:recommended",
    "@vue/eslint-config-typescript",
    "@vue/eslint-config-prettier",
  ],
  parserOptions: {
    ecmaVersion: "latest",
  },
  // needed to prevent false-positives for “missing” interfaces
  // defined in global.d.ts and referenced in .vue files
  overrides: [
    {
      files: ["*.ts", "*.vue"],
      rules: {
        "no-undef": "off",
      },
    },
  ],
};
