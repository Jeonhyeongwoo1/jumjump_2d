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
