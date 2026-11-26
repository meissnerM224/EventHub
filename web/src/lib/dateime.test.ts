import { localInputToUtcIso } from "./datetime";

describe("localInputToUtcIso", () => {
    it("returns a UTC ISO string for the same moment", () => {
        const iso = localInputToUtcIso("2030-06-15T20:00");

        expect(iso).toMatch(/Z$/);
        expect(new Date(iso).getTime()).toBe(
            new Date(2030, 5, 15, 20, 0).getTime(), // local time, month 0-based
        );
    });
});