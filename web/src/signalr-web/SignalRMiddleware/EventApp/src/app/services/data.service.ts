import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { ImageItem } from '../components/items/image.item';
import { TextItem } from '../components/items/text.item';
import { AudioItem } from '../components/items/audio.item';

@Injectable()
export class DataService {
    private userId = new BehaviorSubject<string>("default");
    currentUser = this.userId.asObservable();

    private categoryId = new BehaviorSubject<string>("defaultCategoryId")
    currentCategory = this.categoryId.asObservable();

    private categoryName = new BehaviorSubject<string>("defaultCategoryName")
    currentCategoryName = this.categoryName.asObservable();

    private images = new BehaviorSubject<Array<ImageItem>>([])
    images_list = this.images.asObservable()

    private text = new BehaviorSubject<Array<TextItem>>([])
    text_list = this.text.asObservable()

    private audio = new BehaviorSubject<Array<AudioItem>>([])
    audio_list = this.audio.asObservable()

    constructor(){ }

    getCurrentUser(){
        return this.currentUser;
    }
    
    setCurrentUser(userid: string){
        this.userId.next(userid)
    }

    getCurrentCategory(){
        return this.currentCategory
    }

    setCurrentCategory(categoryId: string){
        this.categoryId.next(categoryId)
    }

    getCurrentCategoryName(){
        return this.currentCategoryName;
    }

    setCurrentCategoryName(categoryName: string){
        this.categoryName.next(categoryName)
    }

    getImages(){
        return this.images_list
    }

    setImages(i_arr:Array<ImageItem>){
        for(let item of i_arr){
            this.images.next(this.images.getValue().concat(item))
        }
    }

    addImage(image:ImageItem){
        this.images.next(this.images.getValue().concat(image))
    }

    clearImages(){
        this.images.next([])
    }

    getText(){
        return this.text_list
    }

    setText(i_txt:Array<TextItem>){
        for(let item of i_txt){
            this.text.next(this.text.getValue().concat(item))
        }
    }

    addText(text:TextItem){
        this.text.next(this.text.getValue().concat(text))
    }

    clearText(){
        this.text.next([])
    }


    getAudio(){
        return this.audio_list
    }

    setAudio(i_audio:Array<AudioItem>){
        for(let item of i_audio){
            this.audio.next(this.audio.getValue().concat(item))
        }
    }

    addAudio(audio:AudioItem){
        this.audio.next(this.audio.getValue().concat(audio))
    }

    clearAudio(){
        this.audio.next([])
    }
}