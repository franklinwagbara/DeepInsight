export declare function shouldMask(element: Element, maskAllInputs: boolean): boolean;
export declare function maskValue(value: string): string;
export declare function maskTextContent(text: string, element: Element, maskAllInputs: boolean): string;
export declare function shouldExclude(element: Element): boolean;
export declare function sanitizeAttributes(element: Element, attrs: Record<string, string>, maskAllInputs: boolean): Record<string, string>;
export declare function anonymizeIp(ip: string): string;
