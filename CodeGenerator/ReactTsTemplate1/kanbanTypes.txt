export interface Attachment {
  id: string;
}

export interface CheckItem {
  id: string;
  checklistId?: string;
  name: string;
  state: "incomplete" | "complete";
}

export interface Checklist {
  id: string;
  checkItems: CheckItem[];
  name: string;
}

export interface Comment {
  id: string;
  cardId: string;
  createdAt: number;
  memberId: string;
  message: string;
}

export interface Card {
  id: string;
  attachments: Attachment[];
  checklists: Checklist[];
  columnId: string;
  comments: Comment[];
  cover: string | null;
  description: string | null;
  due: number | null;
  isSubscribed: boolean;
  labels: string[];
  memberIds: string[];
  name: string;
}

export interface Column {
  id: string;
  cardIds: string[];
  name: string;
}

export interface Member {
  id: string;
  avatar: string | null;
  name: string;
}

export interface Board {
  cards: Card[];
  columns: Column[];
  members: Member[];
}
