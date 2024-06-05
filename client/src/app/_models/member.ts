import { Photo } from "./photo";

export interface Member {
    id: number;
    userName: string;
    photoUrl: string;
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    age: number;
    created: string;
    photos: Photo[];
}