import { BaseEvent, DeepInsightConfig } from '../types/events';
export declare class EventBuffer {
    private buffer;
    private flushTimer;
    private readonly batchSize;
    private readonly batchInterval;
    private onFlush;
    constructor(config: DeepInsightConfig);
    setFlushHandler(handler: (events: BaseEvent[]) => void): void;
    push(event: BaseEvent): void;
    flush(): void;
    size(): number;
    destroy(): void;
}
