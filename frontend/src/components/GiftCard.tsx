import type { PublicGift } from '../api/client';

type Props = {
  gift: PublicGift;
  onOpen: (gift: PublicGift) => void;
};

export function GiftCard({ gift, onOpen }: Props) {
  const isUnavailable = gift.isReserved && !gift.isReservedByCurrentVisitor;
  const classes = [
    'giftCard',
    gift.isReserved ? 'reservedCard' : '',
    isUnavailable ? 'unavailable' : ''
  ].filter(Boolean).join(' ');

  return (
    <button
      className={classes}
      type="button"
      onClick={() => onOpen(gift)}
      disabled={isUnavailable}
    >
      <span className={`giftStatus ${gift.isReserved ? 'reserved' : 'available'}`}>
        {gift.isReserved ? 'Забронирован' : 'Доступен'}
      </span>
      <img src={gift.imageUrl} alt={gift.name} className="giftImage" loading="lazy" />
      <span className="giftName">{gift.name}</span>
    </button>
  );
}
