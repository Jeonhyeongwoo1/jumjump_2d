import {onRequest} from "firebase-functions/v2/https";
import {PUBLIC_HTTP_OPTIONS} from "../shared/constants";
import {markAttendancePromotionClaimed} from "../shared/attendance";
import {RequestError} from "../shared/errors";
import {AttendancePromotionClaimRequest} from "../shared/types";
import {authenticate} from "../shared/user";
import {requireMethod, sendAuthAwareError} from "../shared/utils";

export const claimAttendancePromotion = onRequest(
  PUBLIC_HTTP_OPTIONS,
  async (req, res): Promise<void> => {
    if (!requireMethod(req.method, "POST", res)) {
      return;
    }

    try {
      const auth = await authenticate(req.header("authorization"));
      const request = parseAttendancePromotionClaimRequest(req.body);
      const attendancePromotionClaim = await markAttendancePromotionClaimed(
        auth.userId,
        request.milestoneDay,
      );

      res.status(200).json({attendancePromotionClaim});
    } catch (error) {
      sendAuthAwareError(error, res, "claim_attendance_promotion_failed");
    }
  }
);

function parseAttendancePromotionClaimRequest(
  body: unknown,
): AttendancePromotionClaimRequest {
  if (typeof body !== "object" || body === null) {
    throw new RequestError("invalid_body", 400);
  }

  const values = body as Record<string, unknown>;
  if (values.milestoneDay !== 3 && values.milestoneDay !== 7) {
    throw new RequestError("invalid_milestone_day", 400);
  }

  return {milestoneDay: values.milestoneDay};
}
