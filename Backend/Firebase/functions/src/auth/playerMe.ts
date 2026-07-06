import {onRequest} from "firebase-functions/v2/https";
import {PUBLIC_HTTP_OPTIONS} from "../shared/constants";
import {requireMethod, sendAuthAwareError} from "../shared/utils";
import {authenticate, getUserResponse} from "../shared/user";

export const playerMe = onRequest(
  PUBLIC_HTTP_OPTIONS,
  async (req, res): Promise<void> => {
    if (!requireMethod(req.method, "GET", res)) {
      return;
    }

    try {
      const auth = await authenticate(req.header("authorization"));
      const user = await getUserResponse(auth.userId);

      res.status(200).json({user});
    } catch (error) {
      sendAuthAwareError(error, res, "player_me_failed");
    }
  }
);
