export class TextItem{
    id: string
    text: string
    isActive: boolean = true;
    showNotification: boolean = false;

    constructor(e_id){
        this.id = e_id
    }

    getId(){
        return this.id
    }
    setCategoryId(id: string){
        this.id = id
    }

    getText(){
        return this.text
    }
    setText(e_text:string){
        this.text = e_text
    }
}