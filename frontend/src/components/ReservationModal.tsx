import { FormEvent, useEffect, useRef, useState } from 'react';
import { api, ApiError, GiftDetails } from '../api/client';

type Props = {
  gift: GiftDetails;
  onCancel: () => void;
  onReserved: (gift: GiftDetails) => void;
};

export function ReservationModal({ gift, onCancel, onReserved }: Props) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [name, setName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    inputRef.current?.focus();
  }, []);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onCancel();
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [onCancel]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    const trimmedName = name.trim();
    if (trimmedName.length < 2 || trimmedName.length > 80) {
      setError('Имя должно содержать от 2 до 80 символов.');
      return;
    }

    setBusy(true);
    setError(null);
    try {
      await api.reserveGift(gift.id, trimmedName);
      const updated = await api.getGift(gift.id);
      onReserved(updated);
    } catch (err) {
      setError((err as ApiError).message || 'Не удалось забронировать подарок.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="modalLayer nestedLayer" role="presentation">
      <form className="reserveModal" role="dialog" aria-modal="true" aria-labelledby="reserve-title" onSubmit={submit}>
        <h2 id="reserve-title">Забронировать подарок</h2>
        <label className="fieldLabel">
          Ваше имя
          <input
            ref={inputRef}
            value={name}
            onChange={(event) => setName(event.target.value)}
            minLength={2}
            maxLength={80}
            required
          />
        </label>
        {error && <p className="stateText errorText">{error}</p>}
        <div className="modalActions">
          <button className="primaryButton" type="submit" disabled={busy}>
            Подтвердить
          </button>
          <button className="secondaryButton" type="button" onClick={onCancel} disabled={busy}>
            Отмена
          </button>
        </div>
      </form>
    </div>
  );
}
