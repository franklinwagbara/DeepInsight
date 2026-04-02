import { DeepInsightClient } from "./core/DeepInsight";

export type { DeepInsightConfig, BaseEvent, EventBatch } from "./types/events";
export { DeepInsightClient } from "./core/DeepInsight";

// Global singleton for script-tag usage
const instance = new DeepInsightClient();

export const init = instance.init.bind(instance);
export const track = instance.track.bind(instance);
export const identify = instance.identify.bind(instance);
export const pause = instance.pause.bind(instance);
export const resume = instance.resume.bind(instance);
export const destroy = instance.destroy.bind(instance);
export const getSessionId = instance.getSessionId.bind(instance);

export default instance;
