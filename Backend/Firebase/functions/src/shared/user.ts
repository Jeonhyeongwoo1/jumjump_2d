import {FieldValue} from "firebase-admin/firestore";
import {db} from "./db";
import {COLLECTIONS, DEFAULT_SELECTED_PLAYER_SKIN_ID} from "./constants";
import {AuthError} from "./errors";
import {digest, parseBearerToken} from "./utils";
import {
  AuthenticatedUser,
  PlayerProgressRequest,
  UserResponse,
} from "./types";

export async function getUserResponse(userId: string): Promise<UserResponse> {
  const userSnapshot = await db.collection(COLLECTIONS.users).doc(userId).get();

  if (!userSnapshot.exists) {
    throw new AuthError("user_not_found", 404);
  }

  const entitlementSnapshot = await db
    .collection(COLLECTIONS.entitlements)
    .doc(userId)
    .get();
  const nickname = userSnapshot.get("nickname");
  const highScore = parseStoredProgressNumber(
    userSnapshot.get("highScore"),
    0,
  );
  const gold = parseStoredProgressNumber(userSnapshot.get("gold"), 0);
  const selectedPlayerSkinId = parseStoredProgressNumber(
    userSnapshot.get("selectedPlayerSkinId"),
    DEFAULT_SELECTED_PLAYER_SKIN_ID,
  );
  const createdAtMillis = parseStoredTimestampMillis(
    userSnapshot.get("createdAt"),
  );

  return {
    userId,
    nickname: typeof nickname === "string" && nickname.length > 0 ?
      nickname :
      userId,
    hasRemovedAds: entitlementSnapshot.get("hasRemovedAds") === true,
    highScore,
    gold,
    selectedPlayerSkinId,
    createdAtMillis,
  };
}

export async function findOrCreateUser(
  tossHash: string,
): Promise<UserResponse> {
  const tossHashDigest = digest(tossHash);
  const mappingRef = db
    .collection(COLLECTIONS.userMappings)
    .doc(tossHashDigest);

  const userId = await db.runTransaction(async (transaction) => {
    const mappingSnapshot = await transaction.get(mappingRef);

    if (mappingSnapshot.exists) {
      const mappedUserId = mappingSnapshot.get("userId");

      if (typeof mappedUserId !== "string") {
        throw new Error("invalid_user_mapping");
      }

      const mappedUserRef = db.collection(COLLECTIONS.users).doc(mappedUserId);
      transaction.update(mappedUserRef, {
        lastLoginAt: FieldValue.serverTimestamp(),
      });

      return mappedUserId;
    }

    const userRef = db.collection(COLLECTIONS.users).doc();

    transaction.set(userRef, {
      userId: userRef.id,
      nickname: userRef.id,
      tossHashDigest,
      highScore: 0,
      gold: 0,
      selectedPlayerSkinId: DEFAULT_SELECTED_PLAYER_SKIN_ID,
      createdAt: FieldValue.serverTimestamp(),
      lastLoginAt: FieldValue.serverTimestamp(),
      updatedAt: FieldValue.serverTimestamp(),
    });

    transaction.set(mappingRef, {
      userId: userRef.id,
      createdAt: FieldValue.serverTimestamp(),
    });

    return userRef.id;
  });

  return getUserResponse(userId);
}

export async function savePlayerProgress(
  userId: string,
  progress: PlayerProgressRequest,
): Promise<UserResponse> {
  const userRef = db.collection(COLLECTIONS.users).doc(userId);

  await db.runTransaction(async (transaction) => {
    const userSnapshot = await transaction.get(userRef);

    if (!userSnapshot.exists) {
      throw new AuthError("user_not_found", 404);
    }

    const storedHighScore = parseStoredProgressNumber(
      userSnapshot.get("highScore"),
      0,
    );

    transaction.update(userRef, {
      highScore: Math.max(storedHighScore, progress.highScore),
      gold: progress.gold,
      selectedPlayerSkinId: progress.selectedPlayerSkinId,
      updatedAt: FieldValue.serverTimestamp(),
    });
  });

  return getUserResponse(userId);
}

export async function authenticate(
  authorizationHeader: string | undefined,
): Promise<AuthenticatedUser> {
  const sessionToken = parseBearerToken(authorizationHeader);

  if (sessionToken === null) {
    throw new AuthError("missing_session_token", 401);
  }

  const sessionSnapshot = await db
    .collection(COLLECTIONS.sessions)
    .doc(sessionToken)
    .get();

  if (!sessionSnapshot.exists) {
    throw new AuthError("invalid_session_token", 401);
  }

  const userId = sessionSnapshot.get("userId");
  const expiresAtMillis = sessionSnapshot.get("expiresAtMillis");

  if (typeof userId !== "string" || typeof expiresAtMillis !== "number") {
    throw new AuthError("invalid_session", 401);
  }

  if (expiresAtMillis <= Date.now()) {
    throw new AuthError("expired_session_token", 401);
  }

  return {userId, sessionToken};
}

function parseStoredProgressNumber(value: unknown, fallback: number): number {
  return typeof value === "number" &&
    Number.isSafeInteger(value) &&
    value >= 0 ?
    value :
    fallback;
}

function parseStoredTimestampMillis(value: unknown): number {
  if (typeof value !== "object" || value === null) {
    return 0;
  }

  const timestamp = value as {toMillis?: () => number};
  if (typeof timestamp.toMillis !== "function") {
    return 0;
  }

  const millis = timestamp.toMillis();
  return Number.isSafeInteger(millis) && millis > 0 ? millis : 0;
}
