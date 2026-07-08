export const FUNCTION_REGION = "asia-northeast3";

export const PUBLIC_HTTP_OPTIONS = {
  region: FUNCTION_REGION,
  cors: true,
};

export const SESSION_TTL_MILLIS = 1000 * 60 * 60 * 24 * 7;
export const DAY_MILLIS = 1000 * 60 * 60 * 24;
export const KST_OFFSET_MILLIS = 1000 * 60 * 60 * 9;
export const DEFAULT_SELECTED_PLAYER_SKIN_ID = 1001;

export const COLLECTIONS = {
  users: "users",
  sessions: "sessions",
  userMappings: "userMappings",
  entitlements: "entitlements",
  purchaseLogs: "purchaseLogs",
} as const;
