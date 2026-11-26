import { hasStarted, isPast } from "./eventTime";

const start = "2030-06-15T18:00:00Z";
const at = (iso: string) => new Date(iso).getTime();

describe("hasStarted", () => {
  it("is false before the start", () => {
    expect(hasStarted(start, at("2030-06-15T17:59:00Z"))).toBe(false);
  });

  it("is true from the start on", () => {
    expect(hasStarted(start, at("2030-06-15T18:00:00Z"))).toBe(true);
    expect(hasStarted(start, at("2030-06-16T10:00:00Z"))).toBe(true);
  });
});

describe("isPast", () => {
  it("is false before the start", () => {
    expect(isPast(start, at("2030-06-15T17:59:00Z"))).toBe(false);
  });

  it("is false while running within three hours", () => {
    expect(isPast(start, at("2030-06-15T20:00:00Z"))).toBe(false);
  });

  it("is true three hours after the start", () => {
    expect(isPast(start, at("2030-06-15T21:00:00Z"))).toBe(true);
  });
});
