import { useEffect, useRef, useState } from 'react';
import { api, ApiError, GiftDetails, PublicGift } from '../api/client';
import { ReservationModal } from './ReservationModal';

type Props = {
  gift: PublicGift;
  onClose: () => void;
  onChanged: () => void;
};

export function GiftModal({ gift, onClose, onChanged }: Props) {
  const closeRef = useRef<HTMLButtonElement>(null);
  const [details, setDetails] = useState<GiftDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [showReserve, setShowReserve] = useState(false);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    closeRef.current?.focus();
  }, []);

  useEffect(() => {
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') onClose();
    };
    document.addEventListener('keydown', onKeyDown);
    return () => document.removeEventListener('keydown', onKeyDown);
  }, [onClose]);

  useEffect(() => {
    setLoading(true);
    api
      .getGift(gift.id)
      .then(setDetails)
      .catch((err: ApiError) => setError(err.message))
      .finally(() => setLoading(false));
  }, [gift.id]);

  async function cancelOwnReservation() {
    if (!details || !window.confirm('Вы действительно хотите отменить бронь этого подарка?')) return;

    setBusy(true);
    setError(null);
    try {
      await api.cancelReservation(details.id);
      const updated = await api.getGift(details.id);
      setDetails(updated);
      setMessage('Бронь отменена.');
      onChanged();
    } catch (err) {
      setError((err as ApiError).message || 'Ошибка отмены брони.');
    } finally {
      setBusy(false);
    }
  }

  function handleReserved(updated: GiftDetails) {
    setDetails(updated);
    setMessage('Подарок успешно забронирован.');
    setShowReserve(false);
    onChanged();
  }

  const shown = details ?? gift;

  return (
    <div className="modalLayer" role="presentation">
      <section className="modal" role="dialog" aria-modal="true" aria-labelledby="gift-modal-title">
        <button ref={closeRef} className="iconButton closeButton" type="button" onClick={onClose} aria-label="Закрыть">
          ×
        </button>

        {loading && <p className="stateText">Загружаем подарок...</p>}
        {error && <p className="stateText errorText">{error}</p>}
        {message && <p className="stateText successText">{message}</p>}

        <img src={shown.imageUrl} alt={shown.name} className="modalImage" />
        <h2 id="gift-modal-title">{shown.name}</h2>

        {details?.description && <p className="modalDescription">{details.description}</p>}

        {details && (
          <>
            <a className="primaryButton linkButton" href={details.productUrl} target="_blank" rel="noopener noreferrer">
              Открыть подарок в магазине
            </a>

            <div className="modalActions">
              {!details.isReserved && (
                <button className="primaryButton" type="button" onClick={() => setShowReserve(true)} disabled={busy}>
                  Забронировать
                </button>
              )}
              {details.isReserved && <button className="mutedButton" type="button" disabled>Забронировано</button>}
              {details.isReservedByCurrentVisitor && (
                <button className="secondaryButton" type="button" onClick={cancelOwnReservation} disabled={busy}>
                  Отменить бронь
                </button>
              )}
            </div>
          </>
        )}
      </section>

      {showReserve && details && (
        <ReservationModal
          gift={details}
          onCancel={() => setShowReserve(false)}
          onReserved={handleReserved}
        />
      )}
    </div>
  );
}
