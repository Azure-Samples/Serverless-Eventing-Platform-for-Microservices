export class ImageItem{
    id: string
    categoryId: string
    previewUrl: string
    caption : string
    isActive: boolean = true;
    showImage: boolean = false;
    showNotification: boolean = false;

    constructor(e_id){
        this.id = e_id
    }

    getId(){
        return this.id
    }

    getCategoryId(){
        return this.categoryId
    }
    getPreviewUrl(){
        return this.previewUrl
    }

    setCategoryId(id: string){
        this.id = id
    }
    setPreviewUrl(url:string){
        this.previewUrl = url
        this.showImage = true
    }

    getCaption(){
        return this.caption
    }
    setCaption(e_caption:string){
        this.caption = e_caption
    }
}