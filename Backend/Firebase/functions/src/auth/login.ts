import {FieldValue} from "firebase-admin/firestore";
import {randomUUID} from "crypto";
import {onRequest} from "firebase-functions/v2/https";
import * as logger from "firebase-functions/logger";
import {db} from "../shared/db";
import {
  COLLECTIONS,
  PUBLIC_HTTP_OPTIONS,
  SESSION_TTL_MILLIS,
} from "../shared/constants";
import {requireMethod, sendAuthAwareError} from "../shared/utils";
import {findOrCreateUser} from "../shared/user";

export const login = onRequest(
  PUBLIC_HTTP_OPTIONS,
  async (req, res): Promise<void> => {
    if (!requireMethod(req.method, "POST", res)) {
      return;
    }

    const tossHash = req.body?.tossHash;

    if (!isValidTossHash(tossHash)) {
      res.status(400).json({error: "invalid_toss_hash"});
      return;
    }

    try {
      const user = await findOrCreateUser(tossHash);
      const sessionToken = randomUUID();
      const now = Date.now();

      await db.collection(COLLECTIONS.sessions).doc(sessionToken).set({
        userId: user.userId,
        createdAt: FieldValue.serverTimestamp(),
        expiresAtMillis: now + SESSION_TTL_MILLIS,
      });

      res.status(200).json({sessionToken, user});
    } catch (error) {
      logger.error("login_failed", error);
      sendAuthAwareError(error, res, "login_failed");
    }
  }
);

function isValidTossHash(value: unknown): value is string {
  return typeof value === "string" && value.length >= 10;
}
