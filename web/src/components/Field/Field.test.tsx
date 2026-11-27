import { render, screen } from "@testing-library/react";
import { SelectField, TextAreaField, TextField } from "./Field";

describe("Field", () => {
  it("links the label to the input", () => {
    render(<TextField label="E-Mail" name="email" type="email" required />);

    const input = screen.getByLabelText("E-Mail");
    expect(input).toHaveAttribute("name", "email");
    expect(input).toBeRequired();
  });

  it("uses the hint as accessible description", () => {
    render(<TextField label="Passwort" hint="Mindestens 8 Zeichen" />);

    expect(screen.getByLabelText("Passwort")).toHaveAccessibleDescription(
      "Mindestens 8 Zeichen",
    );
  });

  it("sets no aria-describedby without a hint", () => {
    render(<TextField label="Titel" />);

    expect(screen.getByLabelText("Titel")).not.toHaveAttribute(
      "aria-describedby",
    );
  });

  it("gives two fields with the same label different ids", () => {
    render(
      <>
        <TextField label="Name" />
        <TextField label="Name" />
      </>,
    );

    const [a, b] = screen.getAllByLabelText("Name");
    expect(a.id).not.toBe(b.id);
  });

  it("renders textarea and select with their label", () => {
    render(
      <>
        <TextAreaField label="Beschreibung" />
        <SelectField label="Kategorie" defaultValue="1">
          <option value="1">Konzert</option>
        </SelectField>
      </>,
    );

    expect(screen.getByLabelText("Beschreibung").tagName).toBe("TEXTAREA");
    expect(screen.getByLabelText("Kategorie")).toHaveValue("1");
  });
});
