import {
  useId,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from "react";
import styles from "./Field.module.css";

type FieldProps = {
  label: ReactNode;
  hint?: ReactNode;
  // Layout of the wrapper (grid column, width) – the field itself keeps its own look.
  className?: string;
};

// Label, control and hint share one id scheme: htmlFor and aria-describedby
// are wired up here, so no page has to invent ids like "password-hint".
function useFieldIds(id: string | undefined, hint: ReactNode) {
  const generated = useId();
  const controlId = id ?? generated;
  const hintId = hint ? `${controlId}-hint` : undefined;
  return { controlId, hintId };
}

function Frame({
  label,
  hint,
  className,
  controlId,
  hintId,
  children,
}: FieldProps & {
  controlId: string;
  hintId?: string;
  children: ReactNode;
}) {
  return (
    <div className={[styles.field, className].filter(Boolean).join(" ")}>
      <label htmlFor={controlId} className={styles.label}>
        {label}
      </label>
      {children}
      {hint && (
        <small id={hintId} className={styles.hint}>
          {hint}
        </small>
      )}
    </div>
  );
}

export function TextField({
  label,
  hint,
  className,
  id,
  ...input
}: FieldProps & Omit<InputHTMLAttributes<HTMLInputElement>, "className">) {
  const { controlId, hintId } = useFieldIds(id, hint);
  return (
    <Frame {...{ label, hint, className, controlId, hintId }}>
      <input
        id={controlId}
        aria-describedby={hintId}
        className={styles.control}
        {...input}
      />
    </Frame>
  );
}

export function TextAreaField({
  label,
  hint,
  className,
  id,
  ...textarea
}: FieldProps &
  Omit<TextareaHTMLAttributes<HTMLTextAreaElement>, "className">) {
  const { controlId, hintId } = useFieldIds(id, hint);
  return (
    <Frame {...{ label, hint, className, controlId, hintId }}>
      <textarea
        id={controlId}
        aria-describedby={hintId}
        className={`${styles.control} ${styles.textarea}`}
        {...textarea}
      />
    </Frame>
  );
}

export function SelectField({
  label,
  hint,
  className,
  id,
  children,
  ...select
}: FieldProps & Omit<SelectHTMLAttributes<HTMLSelectElement>, "className">) {
  const { controlId, hintId } = useFieldIds(id, hint);
  return (
    <Frame {...{ label, hint, className, controlId, hintId }}>
      <select
        id={controlId}
        aria-describedby={hintId}
        className={styles.control}
        {...select}
      >
        {children}
      </select>
    </Frame>
  );
}
