import { ImageItem } from '../items/image.item'
import { TextItem } from '../items/text.item'
import { AudioItem } from '../items/audio.item'

export class Category {

    name: string
    id: string
    synonyms: Array<string>
    imageUrl : string
    showCategory: boolean = false;
    editMode: boolean = false;
    isActive: boolean = true;
    images_list : Array<ImageItem>
    text_list: Array<TextItem>
    audio_list:Array<AudioItem>
    notifications: number = 0;
    showNotification: boolean = false;

    constructor(c_id: string, c_name: string){
        this.name = c_name
        this.id = c_id
        this.imageUrl = "assets/images/dummy.png"
        this.images_list = new Array<ImageItem>()
        this.text_list = new Array<TextItem>()
        this.audio_list = new Array<AudioItem>()

    }

    getId(){
        return this.id
    }

    getName(){
        return this.name
    }

    setName(updated_name:string){
        this.name = updated_name
    }

    setSynonyms(event_synonyms: Array<string>){
        this.synonyms = event_synonyms
    }

    getSynonyms(){
        return this.synonyms
    }

    setImage(url : string){
        this.imageUrl = url
        this.showCategory = true
    }

    getImage(){
        return this.imageUrl
    }

    setItemImages(imgs: Array<ImageItem>){
        this.images_list = imgs
    }

    addItemImages(img: ImageItem){
        this.images_list.push(img)
    }
    
    getItemImages(){
        return this.images_list
    }

    setItemText(txts: Array<TextItem>){
        this.text_list = txts
    }

    addItemText(txt: TextItem){
        this.text_list.push(txt)
    }

    getItemText(){
        return this.text_list
    }

    setItemAudio(audio_lst: Array<AudioItem>){
        this.audio_list = audio_lst
    }

    addItemAudio(audio: AudioItem){
        this.audio_list.push(audio)
    }

    getItemAudio(){
        return this.audio_list
    }


    clearNotifications(){
        this.notifications = 0
    }

}