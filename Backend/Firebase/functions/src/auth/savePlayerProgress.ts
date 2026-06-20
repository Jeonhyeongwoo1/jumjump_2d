import {onRequest} from "firebase-functions/v2/https";
import {PUBLIC_HTTP_OPTIONS} from "../shared/constants";
import {
  requireMethod,
  sendAuthAwareError,
  parseRequiredSafeInteger,
} from "../shared/utils";
import {authenticate, savePlayerProgress as saveProgress} from "../shared/user";
import {RequestError} from "../shared/errors";
import {PlayerProgressRequest} from "../shared/types";

export const savePlayerProgress = onRequest(
  PUBLIC_HTTP_OPTIONS,
  async (req, res): Promise<void> => {
    if (!requireMethod(req.method, "POST", res)) {
      return;
    }

    try {
      const auth = await authenticate(req.header("authorization"));
      const progress = parsePlayerProgressRequest(req.body);
      const user = await saveProgress(auth.userId, progress);

      res.status(200).json({user});
    } catch (error) {
      sendAuthAwareError(error, res, "save_player_progress_failed");
    }
  }
);

function parsePlayerProgressRequest(body: unknown): PlayerProgressRequest {
  if (typeof body !== "object" || body === null) {
    throw new RequestError("invalid_body", 400);
  }

  const values = body as Record<string, unknown>;
  const highScore = parseRequiredSafeInteger(
    values.highScore,
    "high_score",
    0,
  );
  const gold = parseRequiredSafeInteger(values.gold, "gold", 0);
  const selectedPlayerSkinId = parseRequiredSafeInteger(
    values.selectedPlayerSkinId,
    "selected_player_skin_id",
    1,
  );

  return {highScore, gold, selectedPlayerSkinId};
}
