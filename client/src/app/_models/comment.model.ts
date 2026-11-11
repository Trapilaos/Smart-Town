export interface Comment {
    id: number;
    content: string;
    userId: string;
    /** Author's username, returned by the moderation list. */
    username?: string | null;
    date: Date;
    seen?: boolean;
}
