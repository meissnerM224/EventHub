import { formatDate } from "./format";

describe("formatDate", () => {
  const iso = "2026-07-18T17:00:00Z";

  it("formats in the given locale", () => {
    expect(formatDate(iso, "de")).toMatch(/Juli/);
    expect(formatDate(iso, "en")).toMatch(/Jul/);
    expect(formatDate(iso, "en")).not.toMatch(/Juli/);
  });

  it("uses Berlin time", () => {
    // 17:00 UTC is 19:00 in Berlin during summer time.
    expect(formatDate(iso, "de")).toMatch(/19:00/);
  });
});
