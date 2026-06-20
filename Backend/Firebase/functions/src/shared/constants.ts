export const FUNCTION_REGION = "asia-northeast3";

export const PUBLIC_HTTP_OPTIONS = {
  region: FUNCTION_REGION,
  cors: true,
};

export const SESSION_TTL_MILLIS = 1000 * 60 * 60 * 24 * 7;

export const COLLECTIONS = {
  users: "users",
  sessions: "sessions",
  userMappings: "userMappings",
  entitlements: "entitlements",
  purchaseLogs: "purchaseLogs",
} as const;
