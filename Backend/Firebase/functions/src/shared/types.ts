export type AuthenticatedUser = {
  userId: string;
  sessionToken: string;
};

export type UserResponse = {
  userId: string;
  nickname: string;
  hasRemovedAds: boolean;
};

export type AdRemovalPurchaseRequest = {
  orderId: string;
  productId: string;
  purchasedAtMillis?: number;
};
