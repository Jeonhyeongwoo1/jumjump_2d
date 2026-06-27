import {FieldValue} from "firebase-admin/firestore";
import {onRequest} from "firebase-functions/v2/https";
import {db} from "../shared/db";
import {
  COLLECTIONS,
  PUBLIC_HTTP_OPTIONS,
} from "../shared/constants";
import {
  requireMethod,
  sendAuthAwareError,
  parseRequiredString,
  parseOptionalSafeInteger,
} from "../shared/utils";
import {authenticate, getUserResponse} from "../shared/user";
import {RequestError} from "../shared/errors";
import {AdRemovalPurchaseRequest} from "../shared/types";

export const recordAdRemovalPurchase = onRequest(
  PUBLIC_HTTP_OPTIONS,
  async (req, res): Promise<void> => {
    if (!requireMethod(req.method, "POST", res)) {
      return;
    }

    try {
      const auth = await authenticate(req.header("authorization"));
      const purchase = parsePurchaseRequest(req.body);
      const purchaseRef = db
        .collection(COLLECTIONS.users)
        .doc(auth.userId)
        .collection(COLLECTIONS.purchaseLogs)
        .doc(purchase.orderId);
      const entitlementRef = db
        .collection(COLLECTIONS.entitlements)
        .doc(auth.userId);

      await db.runTransaction(async (transaction) => {
        const purchaseSnapshot = await transaction.get(purchaseRef);

        if (!purchaseSnapshot.exists) {
          transaction.set(purchaseRef, {
            orderId: purchase.orderId,
            productId: purchase.productId,
            type: "ad_removal",
            status: "recorded",
            purchasedAtMillis: purchase.purchasedAtMillis ?? Date.now(),
            createdAt: FieldValue.serverTimestamp(),
          });
        }

        transaction.set(
          entitlementRef,
          {
            userId: auth.userId,
            hasRemovedAds: true,
            adRemovalOrderId: purchase.orderId,
            updatedAt: FieldValue.serverTimestamp(),
          },
          {merge: true}
        );
      });

      const user = await getUserResponse(auth.userId);
      res.status(200).json({user});
    } catch (error) {
      sendAuthAwareError(error, res, "record_ad_removal_purchase_failed");
    }
  }
);

function parsePurchaseRequest(body: unknown): AdRemovalPurchaseRequest {
  if (typeof body !== "object" || body === null) {
    throw new RequestError("invalid_body", 400);
  }

  const values = body as Record<string, unknown>;
  const orderId = parseRequiredString(values.orderId, "order_id");
  const productId = parseRequiredString(values.productId, "product_id");
  const purchasedAtMillis = parseOptionalSafeInteger(
    values.purchasedAtMillis,
    "purchased_at_millis",
    0,
  );

  return {orderId, productId, purchasedAtMillis};
}
