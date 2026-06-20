import {FieldValue} from "firebase-admin/firestore";
import {db} from "./db";
import {COLLECTIONS} from "./constants";
import {AuthError} from "./errors";
import {digest, parseBearerToken} from "./utils";
import {AuthenticatedUser, UserResponse} from "./types";

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

  return {
    userId,
    nickname: typeof nickname === "string" && nickname.length > 0 ?
      nickname :
      userId,
    hasRemovedAds: entitlementSnapshot.get("hasRemovedAds") === true,
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
