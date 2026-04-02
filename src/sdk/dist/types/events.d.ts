/** Configuration options for Deep Insight SDK */
export interface DeepInsightConfig {
    projectId: string;
    endpoint: string;
    maskAllInputs?: boolean;
    sampleRate?: number;
    batchSize?: number;
    batchInterval?: number;
    throttleMouseMove?: number;
    enableDomCapture?: boolean;
    enableMouseTracking?: boolean;
    enableScrollTracking?: boolean;
    enableInputTracking?: boolean;
    respectDoNotTrack?: boolean;
}
export declare const enum EventType {
    PageView = "pageview",
    Click = "click",
    Scroll = "scroll",
    MouseMove = "mousemove",
    Input = "input",
    Navigation = "navigation",
    DomMutation = "dom_mutation",
    DomSnapshot = "dom_snapshot",
    Custom = "custom",
    RageClick = "rage_click",
    DeadClick = "dead_click",
    ViewportResize = "viewport_resize",
    Visibility = "visibility"
}
export interface BaseEvent {
    sessionId: string;
    timestamp: number;
    type: string;
    pageUrl: string;
    data: Record<string, unknown>;
}
export interface ClickEventData {
    x: number;
    y: number;
    xPct: number;
    yPct: number;
    element: string;
    selector: string;
    text?: string;
}
export interface ScrollEventData {
    scrollTop: number;
    scrollHeight: number;
    viewportHeight: number;
    depthPct: number;
}
export interface MouseMoveEventData {
    x: number;
    y: number;
    xPct: number;
    yPct: number;
}
export interface InputEventData {
    element: string;
    selector: string;
    value: string;
    masked: boolean;
}
export interface DomMutationData {
    mutations: SerializedMutation[];
}
export interface SerializedMutation {
    type: 'childList' | 'attributes' | 'characterData';
    target: string;
    targetSelector: string;
    addedNodes?: SerializedNode[];
    removedNodes?: string[];
    attributeName?: string;
    attributeValue?: string | null;
    oldValue?: string | null;
}
export interface SerializedNode {
    nodeType: number;
    tagName?: string;
    attributes?: Record<string, string>;
    textContent?: string;
    childNodes?: SerializedNode[];
}
export interface DomSnapshotData {
    html: string;
    width: number;
    height: number;
    href: string;
}
export interface ViewportResizeData {
    width: number;
    height: number;
}
export interface NavigationData {
    from: string;
    to: string;
}
export interface CustomEventData {
    name: string;
    properties?: Record<string, unknown>;
}
export interface EventBatch {
    projectId: string;
    sessionId: string;
    events: BaseEvent[];
    sentAt: number;
    sdkVersion: string;
}
