const MASK_VALUE = "••••••";

const SENSITIVE_TYPES = new Set([
  "password",
  "credit-card",
  "cc-number",
  "cc-exp",
  "cc-csc",
  "ssn",
]);

const SENSITIVE_NAMES =
  /password|passwd|secret|token|ssn|social|credit.?card|cc.?num/i;

export function shouldMask(element: Element, maskAllInputs: boolean): boolean {
  if (element.hasAttribute("data-di-mask")) return true;
  if (element.closest("[data-di-mask]")) return true;

  if (element instanceof HTMLInputElement) {
    const type = element.type.toLowerCase();
    if (SENSITIVE_TYPES.has(type) || type === "password") return true;

    const name = element.name || element.id || "";
    if (SENSITIVE_NAMES.test(name)) return true;

    const autocomplete = element.getAttribute("autocomplete") || "";
    if (SENSITIVE_TYPES.has(autocomplete)) return true;

    if (maskAllInputs) return true;
  }

  return false;
}

export function maskValue(value: string): string {
  if (!value) return value;
  return MASK_VALUE;
}

export function maskTextContent(
  text: string,
  element: Element,
  maskAllInputs: boolean,
): string {
  if (shouldMask(element, maskAllInputs)) {
    return MASK_VALUE;
  }
  return text;
}

export function shouldExclude(element: Element): boolean {
  return element.hasAttribute("data-di-exclude");
}

export function sanitizeAttributes(
  element: Element,
  attrs: Record<string, string>,
  maskAllInputs: boolean,
): Record<string, string> {
  const sanitized = { ...attrs };
  if (shouldMask(element, maskAllInputs)) {
    if ("value" in sanitized) {
      sanitized["value"] = MASK_VALUE;
    }
    if ("placeholder" in sanitized) {
      sanitized["placeholder"] = MASK_VALUE;
    }
  }
  return sanitized;
}

export function anonymizeIp(ip: string): string {
  if (ip.includes(":")) {
    // IPv6: zero last 80 bits
    const parts = ip.split(":");
    if (parts.length >= 4) {
      return parts.slice(0, 3).join(":") + ":0:0:0:0:0";
    }
  }
  // IPv4: zero last octet
  const parts = ip.split(".");
  if (parts.length === 4) {
    parts[3] = "0";
    return parts.join(".");
  }
  return ip;
}
