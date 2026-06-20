import {createHash} from "crypto";
import * as logger from "firebase-functions/logger";
import {RequestError} from "./errors";

export function requireMethod(
  actual: string,
  expected: string,
  res: {status: (code: number) => {json: (body: object) => void}},
): boolean {
  if (actual === expected) {
    return true;
  }

  res.status(405).json({error: "method_not_allowed"});
  return false;
}

export function parseBearerToken(
  authorizationHeader: string | undefined,
): string | null {
  if (typeof authorizationHeader !== "string") {
    return null;
  }

  const [scheme, token] = authorizationHeader.split(" ");

  if (scheme !== "Bearer" || typeof token !== "string" || token.length === 0) {
    return null;
  }

  return token;
}

export function digest(value: string): string {
  return createHash("sha256").update(value).digest("hex");
}

export function sendAuthAwareError(
  error: unknown,
  res: {status: (code: number) => {json: (body: object) => void}},
  logKey: string,
): void {
  if (error instanceof RequestError) {
    res.status(error.statusCode).json({error: error.message});
    return;
  }

  logger.error(logKey, error);
  res.status(500).json({error: logKey});
}

export function parseRequiredString(value: unknown, fieldName: string): string {
  if (typeof value !== "string" || value.length === 0) {
    throw new RequestError(`invalid_${fieldName}`, 400);
  }

  return value;
}

export function parseOptionalSafeInteger(
  value: unknown,
  fieldName: string,
  minValue: number,
): number | undefined {
  if (typeof value === "undefined") {
    return undefined;
  }

  if (
    typeof value !== "number" ||
    !Number.isInteger(value) ||
    value < minValue
  ) {
    throw new RequestError(`invalid_${fieldName}`, 400);
  }

  return value;
}

export function parseRequiredSafeInteger(
  value: unknown,
  fieldName: string,
  minValue: number,
): number {
  if (
    typeof value !== "number" ||
    !Number.isInteger(value) ||
    value < minValue
  ) {
    throw new RequestError(`invalid_${fieldName}`, 400);
  }

  return value;
}
