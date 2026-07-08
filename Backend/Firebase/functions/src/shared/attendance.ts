import {FieldValue} from "firebase-admin/firestore";
import {db} from "./db";
import {
  COLLECTIONS,
  DAY_MILLIS,
  KST_OFFSET_MILLIS,
} from "./constants";
import {AuthError, RequestError} from "./errors";
import {
  AttendancePromotionClaimResponse,
  AttendanceState,
  DailyPlayResponse,
} from "./types";

const EmptyAttendanceState: AttendanceState = {
  lastDateKey: "",
  currentStreak: 0,
  maxStreak: 0,
  totalRecordedDays: 0,
  day3Claimed: false,
  day7Claimed: false,
};

export async function recordDailyPlay(
  userId: string,
  nowMillis = Date.now(),
): Promise<DailyPlayResponse> {
  const todayDateKey = toKstDateKey(nowMillis);
  const yesterdayDateKey = toKstDateKey(nowMillis - DAY_MILLIS);
  const userRef = db.collection(COLLECTIONS.users).doc(userId);

  return db.runTransaction(async (transaction) => {
    const userSnapshot = await transaction.get(userRef);

    if (!userSnapshot.exists) {
      throw new AuthError("user_not_found", 404);
    }

    const storedAttendance = parseStoredAttendanceState(
      userSnapshot.get("attendance"),
    );
    const alreadyRecordedToday =
      storedAttendance.lastDateKey === todayDateKey;
    const nextAttendance = alreadyRecordedToday ?
      storedAttendance :
      advanceAttendanceState(
        storedAttendance,
        todayDateKey,
        yesterdayDateKey,
      );

    if (!alreadyRecordedToday) {
      transaction.update(userRef, {
        attendance: {
          ...nextAttendance,
          updatedAt: FieldValue.serverTimestamp(),
        },
        updatedAt: FieldValue.serverTimestamp(),
      });
    }

    return toDailyPlayResponse(
      nextAttendance,
      todayDateKey,
      alreadyRecordedToday,
    );
  });
}

export async function markAttendancePromotionClaimed(
  userId: string,
  milestoneDay: 3 | 7,
): Promise<AttendancePromotionClaimResponse> {
  const userRef = db.collection(COLLECTIONS.users).doc(userId);

  return db.runTransaction(async (transaction) => {
    const userSnapshot = await transaction.get(userRef);

    if (!userSnapshot.exists) {
      throw new AuthError("user_not_found", 404);
    }

    const attendance = parseStoredAttendanceState(
      userSnapshot.get("attendance"),
    );

    if (attendance.currentStreak < milestoneDay) {
      throw new RequestError("attendance_milestone_not_reached", 400);
    }

    const nextAttendance = {
      ...attendance,
      day3Claimed: milestoneDay === 3 ? true : attendance.day3Claimed,
      day7Claimed: milestoneDay === 7 ? true : attendance.day7Claimed,
    };

    transaction.update(userRef, {
      attendance: {
        ...nextAttendance,
        updatedAt: FieldValue.serverTimestamp(),
      },
      updatedAt: FieldValue.serverTimestamp(),
    });

    return {
      milestoneDay,
      currentStreak: nextAttendance.currentStreak,
      day3Claimed: nextAttendance.day3Claimed,
      day7Claimed: nextAttendance.day7Claimed,
    };
  });
}

function advanceAttendanceState(
  current: AttendanceState,
  todayDateKey: string,
  yesterdayDateKey: string,
): AttendanceState {
  const nextStreak = current.lastDateKey === yesterdayDateKey ?
    current.currentStreak + 1 :
    1;

  return {
    ...current,
    lastDateKey: todayDateKey,
    currentStreak: nextStreak,
    maxStreak: Math.max(current.maxStreak, nextStreak),
    totalRecordedDays: current.totalRecordedDays + 1,
  };
}

function parseStoredAttendanceState(value: unknown): AttendanceState {
  if (typeof value !== "object" || value === null) {
    return EmptyAttendanceState;
  }

  const data = value as Record<string, unknown>;

  return {
    lastDateKey: parseDateKey(data.lastDateKey),
    currentStreak: parseNonNegativeInteger(data.currentStreak),
    maxStreak: parseNonNegativeInteger(data.maxStreak),
    totalRecordedDays: parseNonNegativeInteger(data.totalRecordedDays),
    day3Claimed: data.day3Claimed === true,
    day7Claimed: data.day7Claimed === true,
  };
}

function parseDateKey(value: unknown): string {
  if (typeof value !== "string") {
    return "";
  }

  return /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : "";
}

function parseNonNegativeInteger(value: unknown): number {
  return typeof value === "number" &&
    Number.isSafeInteger(value) &&
    value >= 0 ?
    value :
    0;
}

function toDailyPlayResponse(
  attendance: AttendanceState,
  todayDateKey: string,
  alreadyRecordedToday: boolean,
): DailyPlayResponse {
  return {
    todayDateKey,
    currentStreak: attendance.currentStreak,
    maxStreak: attendance.maxStreak,
    totalRecordedDays: attendance.totalRecordedDays,
    alreadyRecordedToday,
    eligibleDay3Promotion:
      attendance.currentStreak >= 3 && !attendance.day3Claimed,
    eligibleDay7Promotion:
      attendance.currentStreak >= 7 && !attendance.day7Claimed,
    day3Claimed: attendance.day3Claimed,
    day7Claimed: attendance.day7Claimed,
  };
}

function toKstDateKey(millis: number): string {
  return new Date(millis + KST_OFFSET_MILLIS).toISOString().slice(0, 10);
}
