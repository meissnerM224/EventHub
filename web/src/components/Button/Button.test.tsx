import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { Button, ButtonLink } from "./Button";

describe("Button", () => {
  it("defaults to type=button so it never submits a form by accident", async () => {
    const onSubmit = vi.fn((e: Event) => e.preventDefault());
    render(
      <form onSubmit={(e) => onSubmit(e.nativeEvent)}>
        <Button>Klick</Button>
      </form>,
    );

    const button = screen.getByRole("button", { name: "Klick" });
    expect(button).toHaveAttribute("type", "button");
    await userEvent.click(button);
    expect(onSubmit).not.toHaveBeenCalled();
  });

  it("submits when type=submit is set", async () => {
    const onSubmit = vi.fn((e: Event) => e.preventDefault());
    render(
      <form onSubmit={(e) => onSubmit(e.nativeEvent)}>
        <Button type="submit">Senden</Button>
      </form>,
    );

    await userEvent.click(screen.getByRole("button", { name: "Senden" }));
    expect(onSubmit).toHaveBeenCalledOnce();
  });

  it("keeps the caller's className next to its own", () => {
    render(<Button className="extra">X</Button>);

    expect(screen.getByRole("button")).toHaveClass("extra");
  });
});

describe("ButtonLink", () => {
  it("renders a link to the target", () => {
    render(
      <MemoryRouter>
        <ButtonLink to="/login" variant="secondary">
          Anmelden
        </ButtonLink>
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "Anmelden" })).toHaveAttribute(
      "href",
      "/login",
    );
  });
});
