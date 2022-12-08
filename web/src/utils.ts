export class AssertError extends Error {}

export function assert<T>(value: T, message: string, param?: any) {
  if (!value) {
    if (param) {
      message += param;
    }

    throw new AssertError(message);
  }
}

