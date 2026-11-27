import { ApiError } from "./client";
import { errorMessageId } from "./errors";

describe("errorMessageId", () => {
  it("uses the domain code first", () => {
    expect(
      errorMessageId(new ApiError(409, "x", "category.already_exists")),
    ).toBe("error.category.already_exists");
  });

  it("falls back to the status when there is no code", () => {
    expect(errorMessageId(new ApiError(401, "x"))).toBe(
      "error.status.unauthorized",
    );
    expect(errorMessageId(new ApiError(403, "x"))).toBe(
      "error.status.forbidden",
    );
    expect(errorMessageId(new ApiError(404, "x"))).toBe(
      "error.status.not_found",
    );
    expect(errorMessageId(new ApiError(413, "x"))).toBe(
      "error.status.too_large",
    );
    expect(errorMessageId(new ApiError(502, "x"))).toBe("error.status.server");
  });

  it("falls back to the status for an unknown code", () => {
    expect(errorMessageId(new ApiError(500, "x", "something.new"))).toBe(
      "error.status.server",
    );
  });

  it("uses the generic text for other statuses and non-API errors", () => {
    expect(errorMessageId(new ApiError(400, "x"))).toBe("common.error");
    expect(errorMessageId(new TypeError("Failed to fetch"))).toBe(
      "common.error",
    );
  });
});
