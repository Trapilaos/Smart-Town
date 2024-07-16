export interface Comment {
    id: number;
    content: string;
    userId: string;
    date: Date;
    seen?: boolean;
}
