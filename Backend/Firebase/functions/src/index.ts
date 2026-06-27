import {initializeApp} from "firebase-admin/app";
import {setGlobalOptions} from "firebase-functions/v2";

initializeApp();
setGlobalOptions({maxInstances: 10});

export {login} from "./auth/login";
export {playerMe} from "./auth/playerMe";
export {recordAdRemovalPurchase} from "./auth/recordAdRemovalPurchase";
export {savePlayerProgress} from "./auth/savePlayerProgress";
