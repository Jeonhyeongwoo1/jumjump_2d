import {onRequest} from "firebase-functions/v2/https";
import {PUBLIC_HTTP_OPTIONS} from "../shared/constants";
import {recordDailyPlay as recordPlay} from "../shared/attendance";
import {authenticate} from "../shared/user";
import {requireMethod, sendAuthAwareError} from "../shared/utils";

export const recordDailyPlay = onRequest(
  PUBLIC_HTTP_OPTIONS,
  async (req, res): Promise<void> => {
    if (!requireMethod(req.method, "POST", res)) {
      return;
    }

    try {
      const auth = await authenticate(req.header("authorization"));
      const dailyPlay = await recordPlay(auth.userId);

      res.status(200).json({dailyPlay});
    } catch (error) {
      sendAuthAwareError(error, res, "record_daily_play_failed");
    }
  }
);
