export type AuthenticatedUser = {
  userId: string;
  sessionToken: string;
};

export type UserResponse = {
  userId: string;
  nickname: string;
  hasRemovedAds: boolean;
  highScore: number;
  gold: number;
  selectedPlayerSkinId: number;
  createdAtMillis: number;
};

export type AdRemovalPurchaseRequest = {
  orderId: string;
  productId: string;
  purchasedAtMillis?: number;
};

export type PlayerProgressRequest = {
  highScore: number;
  gold: number;
  selectedPlayerSkinId: number;
};

export type AttendanceState = {
  lastDateKey: string;
  currentStreak: number;
  maxStreak: number;
  totalRecordedDays: number;
  day3Claimed: boolean;
  day7Claimed: boolean;
};

export type DailyPlayResponse = {
  todayDateKey: string;
  currentStreak: number;
  maxStreak: number;
  totalRecordedDays: number;
  alreadyRecordedToday: boolean;
  eligibleDay3Promotion: boolean;
  eligibleDay7Promotion: boolean;
  day3Claimed: boolean;
  day7Claimed: boolean;
};

export type AttendancePromotionClaimRequest = {
  milestoneDay: 3 | 7;
};

export type AttendancePromotionClaimResponse = {
  milestoneDay: 3 | 7;
  currentStreak: number;
  day3Claimed: boolean;
  day7Claimed: boolean;
};
