import { invokeCommand } from "./utils";

const gitBranch = invokeCommand("git", ["branch", "--show-current"]);
const gitCommit = invokeCommand("git", ["rev-parse", "--short", "HEAD"]);

process.env.VITE__GIT_BRANCH = gitBranch!;
process.env.VITE__GIT_COMMIT = gitCommit!;
